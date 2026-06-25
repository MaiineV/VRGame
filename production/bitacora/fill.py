#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
fill.py — Rellena el Anexo A de la bitácora con el historial Git completo.

Lee `bitacora-src.html`, busca el placeholder <!--APPENDIX_ROWS--> y lo
reemplaza por filas <tr> generadas desde `git log --shortstat`. Escribe el
resultado en `Bitacora-Pour-Decisions.html`.

Uso:
    python fill.py
Luego renderizar a PDF con Chrome headless:
    chrome --headless --disable-gpu --print-to-pdf=Bitacora-Pour-Decisions.pdf \
           --no-pdf-header-footer Bitacora-Pour-Decisions.html
"""
import subprocess
import sys
import html
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(HERE, "bitacora-src.html")
OUT = os.path.join(HERE, "Bitacora-Pour-Decisions.html")

US = "\x1f"  # unit separator entre campos del meta
REC = "@@COMMIT@@"  # marca de inicio de cada commit


def git_log():
    fmt = REC + "%h" + US + "%ad" + US + "%an" + US + "%s" + US + "%P"
    cmd = ["git", "-C", REPO, "log", "--shortstat", "--date=short", "--format=" + fmt]
    res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
    if res.returncode != 0:
        sys.exit("git log falló:\n" + res.stderr)
    return res.stdout


def parse(raw):
    """Devuelve lista de dicts: hash, date, author, subject, merge(bool), changes(str)."""
    commits = []
    blocks = raw.split(REC)
    stat_re = re.compile(
        r"(\d+) files? changed(?:, (\d+) insertions?\(\+\))?(?:, (\d+) deletions?\(-\))?"
    )
    for block in blocks:
        block = block.strip("\n")
        if not block:
            continue
        lines = block.split("\n")
        meta = lines[0].split(US)
        if len(meta) < 5:
            continue
        h, date, author, subject, parents = meta[0], meta[1], meta[2], meta[3], meta[4]
        is_merge = len(parents.split()) > 1
        # buscar la línea de shortstat (puede venir tras una línea en blanco)
        changes = "—"
        for ln in lines[1:]:
            m = stat_re.search(ln)
            if m:
                files = m.group(1)
                ins = m.group(2)
                dele = m.group(3)
                parts = ["{} arch.".format(files)]
                if ins:
                    parts.append("{} (+)".format(ins))
                if dele:
                    parts.append("{} (−)".format(dele))
                changes = ", ".join(parts)
                break
        if is_merge and changes == "—":
            changes = "—"
        commits.append({
            "hash": h, "date": date, "author": author,
            "subject": subject, "merge": is_merge, "changes": changes,
        })
    return commits


def normalize_author(name):
    # Agustín / Ignacio comparten máquina; se respeta el nombre del commit.
    return name


def rows_html(commits):
    out = []
    for c in commits:
        merge_tag = '<span class="mtag">merge</span>' if c["merge"] else ""
        subject = html.escape(c["subject"])
        out.append(
            '<tr>'
            '<td class="hash">{hash}</td>'
            '<td class="date">{date}</td>'
            '<td class="auth">{auth}</td>'
            '<td class="msg">{subj}{merge}</td>'
            '<td class="chg">{chg}</td>'
            '</tr>'.format(
                hash=html.escape(c["hash"]),
                date=html.escape(c["date"]),
                auth=html.escape(normalize_author(c["author"])),
                subj=subject,
                merge=(" " + merge_tag) if merge_tag else "",
                chg=html.escape(c["changes"]),
            )
        )
    return "\n".join(out)


def main():
    commits = parse(git_log())
    n = len(commits)
    print("Commits leídos:", n)
    with open(SRC, "r", encoding="utf-8") as f:
        doc = f.read()
    if "<!--APPENDIX_ROWS-->" not in doc:
        sys.exit("No se encontró el placeholder <!--APPENDIX_ROWS--> en bitacora-src.html")
    doc = doc.replace("<!--APPENDIX_ROWS-->", rows_html(commits))
    # sincronizar el conteo de commits del anexo por las dudas
    doc = doc.replace("{{COMMIT_COUNT}}", str(n))
    with open(OUT, "w", encoding="utf-8") as f:
        f.write(doc)
    print("Escrito:", OUT)


if __name__ == "__main__":
    main()
