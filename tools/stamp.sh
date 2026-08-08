#!/bin/sh
# Body stamp for versioned docs.
#   line 1:  <!-- v3.1 | body:0a1b2c3 -->
# The hash covers the file WITHOUT line 1, with CR stripped, so writing the
# stamp never changes what the stamp measures.
#
#   sh tools/stamp.sh <file>...            rewrite the stamp in place
#   sh tools/stamp.sh --check <file>...    exit 1 if any stamp is stale
set -eu

check=0
if [ "${1:-}" = "--check" ]; then check=1; shift; fi
[ $# -gt 0 ] || { echo "stamp: no files given" >&2; exit 2; }

status=0
for f in "$@"; do
  [ -f "$f" ] || { echo "stamp: missing $f" >&2; exit 2; }

  line1=$(head -n 1 "$f" | tr -d '\r')
  ver=$(printf '%s' "$line1" | sed -n 's/^<!-- \(v[^ ]*\) | body:[0-9a-f]* -->$/\1/p')
  if [ -z "$ver" ]; then
    echo "stamp: $f has no stamp on line 1 (expected '<!-- vX.Y | body:xxxxxxx -->')" >&2
    exit 2
  fi

  new=$(tail -n +2 "$f" | tr -d '\r' | git hash-object --stdin | cut -c1-7)
  want="<!-- $ver | body:$new -->"

  if [ "$line1" = "$want" ]; then
    [ $check -eq 1 ] || echo "stamp: $f already current ($new)"
    continue
  fi

  if [ $check -eq 1 ]; then
    echo "stamp: STALE $f — line 1 says '$line1', body is $new" >&2
    status=1
    continue
  fi

  tmp="$f.stamp.tmp"
  printf '%s\n' "$want" > "$tmp"
  tail -n +2 "$f" >> "$tmp"
  mv "$tmp" "$f"
  echo "stamp: $f -> $new"
done
exit $status
