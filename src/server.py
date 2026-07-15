"""
MCP server — Orders database tools.

Environment variables:
  DB_PATH   Path to the SQLite database file.
            Defaults to db/sales.sqlite (local dev).
            Set to /data/orders.db when running in Docker.
"""

import asyncio
import json
import os
import sqlite3
from pathlib import Path

from mcp.server import Server
from mcp.server.stdio import stdio_server
from mcp import types

# ─── Config ────────────────────────────────────────────────────────────────────

DB_PATH = Path(os.getenv("DB_PATH", "db/sales.sqlite"))

# ─── Server ────────────────────────────────────────────────────────────────────

app = Server("orders-mcp-server")


def _get_connection() -> sqlite3.Connection:
    if not DB_PATH.exists():
        raise FileNotFoundError(f"Database not found at '{DB_PATH}'")
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn


# ─── Tool definitions ──────────────────────────────────────────────────────────

@app.list_tools()
async def list_tools() -> list[types.Tool]:
    return [
        types.Tool(
            name="get_orders_by_month",
            description=(
                "Fetch all orders placed in a given month and year. "
                "Returns a JSON array of order records."
            ),
            inputSchema={
                "type": "object",
                "properties": {
                    "year": {
                        "type": "integer",
                        "description": "Four-digit year (e.g. 2025).",
                    },
                    "month": {
                        "type": "integer",
                        "description": "Month as an integer from 1 (January) to 12 (December).",
                        "minimum": 1,
                        "maximum": 12,
                    },
                },
                "required": ["year", "month"],
            },
        )
    ]


# ─── Tool handlers ─────────────────────────────────────────────────────────────

@app.call_tool()
async def call_tool(name: str, arguments: dict) -> list[types.TextContent]:
    if name == "get_orders_by_month":
        return _handle_get_orders_by_month(arguments)
    raise ValueError(f"Unknown tool: '{name}'")


def _handle_get_orders_by_month(args: dict) -> list[types.TextContent]:
    year: int = args["year"]
    month: int = args["month"]

    if not (1 <= month <= 12):
        raise ValueError("month must be between 1 and 12")

    conn = _get_connection()
    try:
        cursor = conn.execute(
            """
            SELECT *
            FROM   orders
            WHERE  strftime('%Y', order_date) = ?
               AND strftime('%m', order_date) = ?
            ORDER  BY order_date
            """,
            (str(year), f"{month:02d}"),
        )
        rows = [dict(row) for row in cursor.fetchall()]
    finally:
        conn.close()

    result = {
        "year": year,
        "month": month,
        "count": len(rows),
        "orders": rows,
    }
    return [types.TextContent(type="text", text=json.dumps(result, indent=2))]


# ─── Entrypoint ────────────────────────────────────────────────────────────────

async def main() -> None:
    async with stdio_server() as (read_stream, write_stream):
        await app.run(
            read_stream,
            write_stream,
            app.create_initialization_options(),
        )


if __name__ == "__main__":
    asyncio.run(main())
