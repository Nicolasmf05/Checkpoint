"""Formatea Python y SQL sin tocar archivos generados ni cambiar tokens de SQL.

El inventario llega desde Format-Code.ps1. Los cuerpos PostgreSQL entre dólares
se conservan porque pueden contener PL/pgSQL, que SQL Formatter no interpreta.
"""

import argparse
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / ".tools" / "format-python"))

import black
from sqlparse import tokens
from sqlparse.lexer import tokenize


def sql_tokens(source):
    """Conserva literales y normaliza la caja de nombres PostgreSQL sin comillas."""
    result = []
    for kind, value in tokenize(source):
        if kind in tokens.Whitespace or kind in tokens.Comment:
            continue
        if kind in tokens.Literal.String:
            result.append(("literal", value))
        else:
            # El lexer puede agrupar [1] o separar sus signos según el espaciado.
            result.extend(
                ("token", part.lower())
                for part in re.findall(r"\w+|->>|->|::|:=|>=|<=|<>|!=|\|\||[^\s]", value)
            )
    return result


def format_file(path, check):
    source = path.read_text(encoding="utf-8-sig")
    if path.suffix == ".py":
        formatted = black.format_file_contents(
            source, fast=False, mode=black.FileMode(line_length=100)
        )
    else:
        formatted = subprocess.run(
            ["node", str(ROOT / "scripts" / "format-sql.mjs"), str(path)],
            capture_output=True,
            text=True,
            encoding="utf-8",
            check=True,
        ).stdout
        if sql_tokens(source) != sql_tokens(formatted):
            raise RuntimeError(f"El formato ha cambiado tokens SQL: {path}")
    if path.read_bytes() == formatted.encode("utf-8"):
        return False
    if not check:
        path.write_bytes(formatted.encode("utf-8"))
    print(path.relative_to(ROOT))
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("files", nargs="+")
    args = parser.parse_args()
    changed = False
    for name in args.files:
        path = (ROOT / name).resolve()
        if not path.is_relative_to(ROOT):
            raise ValueError("El archivo queda fuera del proyecto")
        try:
            changed |= format_file(path, args.check)
        except black.NothingChanged:
            # Black puede detectar igualdad antes de devolver el texto formateado.
            if path.read_bytes().startswith(b"\xef\xbb\xbf") or b"\r\n" in path.read_bytes():
                if args.check:
                    print(path.relative_to(ROOT))
                    changed = True
                else:
                    path.write_bytes(path.read_text(encoding="utf-8-sig").encode("utf-8"))
    return int(args.check and changed)


if __name__ == "__main__":
    raise SystemExit(main())
