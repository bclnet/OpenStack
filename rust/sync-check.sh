#!/usr/bin/env bash
# sync-check.sh — report drift between the C# tree and this Rust port.
#
#   ./sync-check.sh [path-to-dotnet-tree]
#
# For every row in PORT_MAP.tsv it compares the C# file's current hash against
# the PORT-SHA recorded in the corresponding .rs header, and reports:
#
#   STALE    C# changed since the port  -> the Rust file needs the same change
#   MISSING  C# file gone               -> port was deleted or moved upstream
#   NEW      C# file not in the map     -> re-run gen_port.py to add it
#   TODO     mapped but not ported yet
#
# Exit code is non-zero when anything is STALE or MISSING, so this works as a
# CI gate on the C# repo: a PR that touches a ported file fails until the Rust
# side is updated or the PORT-SHA is deliberately bumped.

set -uo pipefail

DOTNET_ROOT="${1:-../src/dotnet}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MAP="$HERE/PORT_MAP.tsv"

[[ -f "$MAP" ]] || { echo "no PORT_MAP.tsv next to $0" >&2; exit 2; }
[[ -d "$DOTNET_ROOT" ]] || { echo "C# tree not found: $DOTNET_ROOT" >&2; exit 2; }

stale=0 missing=0 todo=0 done_ct=0 new_ct=0 partial_ct=0

hash_of() { sha256sum "$1" | cut -c1-16; }

while IFS=$'\t' read -r status loc sha cs rs crate; do
    [[ "$status" == "status" ]] && continue
    cs_abs="$DOTNET_ROOT/$cs"
    rs_abs="$HERE/$rs"

    if [[ ! -f "$cs_abs" ]]; then
        echo "MISSING  $cs"
        missing=$((missing + 1))
        continue
    fi

    # A file counts as ported once its header says so. Files declaring
    # "PARTIAL PORT" are counted separately: the mapping exists and drift is
    # still tracked, but logic remains unported inside them.
    if [[ -f "$rs_abs" ]] && grep -q 'PARTIAL PORT' "$rs_abs"; then
        partial_ct=$((partial_ct + 1))
    fi
    if [[ -f "$rs_abs" ]] && grep -q '^// PORT-STATUS: done' "$rs_abs"; then
        recorded="$(grep -m1 '^// PORT-SHA:' "$rs_abs" | awk '{print $3}')"
        current="$(hash_of "$cs_abs")"
        if [[ "$recorded" != "$current" ]]; then
            echo "STALE    $cs"
            echo "         recorded=$recorded current=$current"
            echo "         update:  $rs"
            stale=$((stale + 1))
        else
            done_ct=$((done_ct + 1))
        fi
    else
        todo=$((todo + 1))
    fi
done < "$MAP"

# .rs files claiming PORT-STATUS: done that no map row points at. Catches a
# hand-written path drifting from the generator's naming (e.g. FFmpegService).
orphan=0
while IFS= read -r rs; do
    rel="${rs#"$HERE"/}"
    # PORT-SHARED marks a module deliberately extracted from a C# file that
    # already has its own mapped .rs — shared by two crates, so it cannot sit at
    # a single 1:1 path. Not an orphan.
    grep -q '^// PORT-SHARED: yes' "$rs" && continue
    grep -qF $'\t'"$rel"$'\t' "$MAP" || { echo "ORPHAN   $rel (done, but not in PORT_MAP)"; orphan=$((orphan + 1)); }
done < <(grep -rl '^// PORT-STATUS: done' --include='*.rs' "$HERE" | sort)

# C# files that exist but were never mapped
while IFS= read -r f; do
    rel="${f#"$DOTNET_ROOT"/}"
    grep -qF $'\t'"$rel"$'\t' "$MAP" || { echo "NEW      $rel"; new_ct=$((new_ct + 1)); }
done < <(find "$DOTNET_ROOT" -name '*.cs' \
            -not -path '*/bin/*' -not -path '*/obj/*' \
            -not -name '*AssemblyInfo.cs' -not -name '.NETStandard*' | sort)

echo
echo "ported $done_ct | todo $todo | stale $stale | missing $missing | unmapped $new_ct | orphan $orphan"
[[ $partial_ct -gt 0 ]] && echo "  of the $done_ct ported, $partial_ct declare PARTIAL PORT (logic still missing inside)"
[[ $((stale + missing + orphan)) -eq 0 ]]
