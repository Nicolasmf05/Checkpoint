#!/usr/bin/env bash
set -euo pipefail
address=${CHECKPOINT_LISTEN_ADDRESS:?}
interface=${CHECKPOINT_INTERFACE:?}
[[ $address =~ ^[0-9.]+$ && $interface =~ ^[a-zA-Z0-9._-]+$ ]] || exit 1
rule=(-i "$interface" -d "$address" -p tcp -m multiport --dports 80,443 -m conntrack --ctstate NEW -m comment --comment checkpoint-web -j ACCEPT)
case ${1:?} in
    add)
        if ! /usr/sbin/iptables -w -C INPUT "${rule[@]}" 2>/dev/null; then
            /usr/sbin/iptables -w -I INPUT 1 "${rule[@]}"
        fi
        ;;
    remove)
        if /usr/sbin/iptables -w -C INPUT "${rule[@]}" 2>/dev/null; then
            /usr/sbin/iptables -w -D INPUT "${rule[@]}"
        fi
        ;;
    *) exit 1 ;;
esac
