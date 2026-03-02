#!/usr/bin/env python3
"""
run-sql-stored-procs.py  –  Deploy stored procedures to Azure SQL.
prompt-006-create-stored-procedures
"""

import os
import re
import struct
import sys
import pyodbc
from azure.identity import AzureCliCredential

# ── Configuration ────────────────────────────────────────────────────────────
SERVER          = os.environ.get("SQL_SERVER_FQDN", "")
DATABASE        = os.environ.get("SQL_DATABASE", "Northwind")
SQL_SCRIPT_FILE = "stored-procedures.sql"

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

    print(f"Connecting to: {SERVER} / {DATABASE}")
    token_struct = get_access_token()
    conn = get_connection(token_struct)
    print(f"Connected. Parsing {SQL_SCRIPT_FILE} …")

    batches = parse_sql_file(SQL_SCRIPT_FILE)
    print(f"Found {len(batches)} SQL batch(es). Executing …\n")

    run_script(conn, batches)
    conn.close()
    print("Done.")


if __name__ == "__main__":
    main()
