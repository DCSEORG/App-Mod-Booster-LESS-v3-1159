#!/usr/bin/env python3
"""
run-sql-dbrole.py  –  Configure database roles for the managed identity.
prompt-005-configure-database-roles

Replaces the MANAGED-IDENTITY-NAME placeholder in script.sql with the actual
managed identity name (from the MANAGED_IDENTITY_NAME environment variable),
then executes the patched SQL against the Northwind database.
"""

import os
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import pyodbc
from azure.identity import AzureCliCredential

# ── Configuration ────────────────────────────────────────────────────────────
SERVER                = os.environ.get("SQL_SERVER_FQDN", "")
DATABASE              = os.environ.get("SQL_DATABASE", "Northwind")
MANAGED_IDENTITY_NAME = os.environ.get("MANAGED_IDENTITY_NAME", "")
SQL_SCRIPT_FILE       = "script.sql"

SQL_COPT_SS_ACCESS_TOKEN = 1256


def get_access_token() -> bytes:
    credential = AzureCliCredential()
    token = credential.get_token("https://database.windows.net/.default")
    token_bytes = token.token.encode("utf-16-le")
    token_struct = struct.pack(f"<I{len(token_bytes)}s", len(token_bytes), token_bytes)
    return token_struct


def get_connection(token_struct: bytes) -> pyodbc.Connection:
    connection_string = (
        f"DRIVER={{ODBC Driver 18 for SQL Server}};"
        f"SERVER={SERVER};"
        f"DATABASE={DATABASE};"
        f"Encrypt=yes;"
        f"TrustServerCertificate=no;"
    )
    conn = pyodbc.connect(
        connection_string,
        attrs_before={SQL_COPT_SS_ACCESS_TOKEN: token_struct},
    )
    conn.autocommit = True
    return conn


def replace_placeholder(src_file: str, identity_name: str) -> str:
    """
    Replace MANAGED-IDENTITY-NAME in src_file and write to a temp file.
    Uses sed with .bak extension for cross-platform (Mac/Linux) compatibility.
    Returns path to the patched temp file.
    """
    tmp = tempfile.NamedTemporaryFile(
        mode="w", suffix=".sql", delete=False, encoding="utf-8"
    )
    tmp_path = tmp.name
    tmp.close()

    shutil.copy2(src_file, tmp_path)

    # sed -i.bak is POSIX-compatible and works on macOS/Linux
    result = subprocess.run(
        [
            "sed", "-i.bak",
            f"s/MANAGED-IDENTITY-NAME/{identity_name}/g",
            tmp_path,
        ],
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        print(f"ERROR: sed failed: {result.stderr}")
        sys.exit(1)

    bak = tmp_path + ".bak"
    if os.path.exists(bak):
        os.remove(bak)

    return tmp_path


def parse_sql_file(filepath: str) -> list[str]:
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    batches = re.split(r"^\s*GO\s*$", content, flags=re.IGNORECASE | re.MULTILINE)
    return [b.strip() for b in batches if b.strip()]


def run_script(conn: pyodbc.Connection, batches: list[str]) -> None:
    cursor = conn.cursor()
    total  = len(batches)
    passed = 0
    failed = 0

    for i, batch in enumerate(batches, start=1):
        preview = batch[:80].replace("\n", " ")
        try:
            cursor.execute(batch)
            print(f"  ✓ [{i}/{total}] {preview}")
            passed += 1
        except pyodbc.Error as exc:
            print(f"  ✗ [{i}/{total}] {preview}")
            print(f"      Error: {exc}")
            failed += 1

    cursor.close()
    print(f"\nCompleted: {passed} succeeded, {failed} failed out of {total} batches.")
    if failed:
        sys.exit(1)


def main() -> None:
    if not SERVER:
        print("ERROR: SQL_SERVER_FQDN environment variable is not set.")
        sys.exit(1)
    if not MANAGED_IDENTITY_NAME:
        print("ERROR: MANAGED_IDENTITY_NAME environment variable is not set.")
        sys.exit(1)

    print(f"Connecting to: {SERVER} / {DATABASE}")
    print(f"Configuring roles for managed identity: {MANAGED_IDENTITY_NAME}")

    patched_file = replace_placeholder(SQL_SCRIPT_FILE, MANAGED_IDENTITY_NAME)
    try:
        token_struct = get_access_token()
        conn         = get_connection(token_struct)
        print(f"Connected. Parsing {patched_file} …")

        batches = parse_sql_file(patched_file)
        print(f"Found {len(batches)} SQL batch(es). Executing …\n")
        run_script(conn, batches)
        conn.close()
    finally:
        if os.path.exists(patched_file):
            os.remove(patched_file)

    print("Done.")


if __name__ == "__main__":
    main()
