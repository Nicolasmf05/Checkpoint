#!/usr/bin/env bash
# Instala y configura el servicio Steam y su proxy HTTPS en el servidor de destino.

set -euo pipefail

# Run as root from the uploaded release directory. Does not read existing secrets.
origin=${1:?HTTPS origin required}
release_id=${2:?Release identifier required}
caddy_version=2.11.6
[[ $origin =~ ^https://[a-zA-Z0-9.-]+$ ]] || {
  printf 'Invalid HTTPS origin\n' >&2
  exit 1
}
[[ $release_id =~ ^[a-zA-Z0-9._-]{1,80}$ ]] || exit 1
[[ $(id -u) == 0 ]] || {
  printf 'Run with sudo\n' >&2
  exit 1
}
[[ $(node -p 'Number(process.versions.node.split(".")[0])') -ge 22 ]] || exit 1
[[ $(uname -m) == aarch64 ]] || {
  printf 'This deployment targets ARM64\n' >&2
  exit 1
}
source_dir=$(pwd -P)
test -f "$source_dir/server/src/index.mjs"
test -f "$source_dir/deploy/firewall.sh"
public_host=${origin#https://}
listen_address=$(ip -4 route get 1.1.1.1 | awk '{for(i=1;i<=NF;i++) if($i=="src") {print $(i+1); exit}}')
interface=$(ip -4 route get 1.1.1.1 | awk '{for(i=1;i<=NF;i++) if($i=="dev") {print $(i+1); exit}}')
[[ $listen_address =~ ^[0-9.]+$ && $interface =~ ^[a-zA-Z0-9._-]+$ ]] || exit 1

node --test server/test/*.test.mjs

for account in checkpoint checkpoint-proxy; do
  if ! id "$account" > /dev/null 2>&1; then
    useradd --system --user-group --home-dir "/var/lib/$account" --shell /usr/sbin/nologin "$account"
  fi
done
install -d -m 0755 /opt/checkpoint/releases /opt/checkpoint/bin
install -d -m 0755 -o root -g checkpoint /etc/checkpoint
release_dir="/opt/checkpoint/releases/$release_id"
test ! -e "$release_dir"
install -d -m 0755 "$release_dir"
cp -R "$source_dir/server" "$source_dir/deploy" "$source_dir/LICENSE" "$release_dir/"
chown -R root:root "$release_dir"
chmod -R u=rwX,go=rX "$release_dir"
chmod 0755 "$release_dir/deploy/firewall.sh"

if test ! -f /opt/checkpoint/bin/caddy; then
  download_dir=$(mktemp -d /tmp/checkpoint-caddy.XXXXXXXX)
  asset="caddy_${caddy_version}_linux_arm64.tar.gz"
  base="https://github.com/caddyserver/caddy/releases/download/v${caddy_version}"
  curl --fail --silent --show-error --location --max-time 120 "$base/$asset" -o "$download_dir/$asset"
  curl --fail --silent --show-error --location --max-time 30 "$base/caddy_${caddy_version}_checksums.txt" -o "$download_dir/checksums.txt"
  (
    cd "$download_dir"
    awk -v name="$asset" '$2 == name {print}' checksums.txt > selected.sha512
    test -s selected.sha512
    sha512sum --check selected.sha512
  )
  tar -xzf "$download_dir/$asset" -C "$download_dir" caddy
  install -m 0755 "$download_dir/caddy" /opt/checkpoint/bin/caddy
fi
/opt/checkpoint/bin/caddy version

if test ! -f /etc/checkpoint/service.env; then
  cat > /etc/checkpoint/service.env << EOF
STEAM_API_KEY=
PUBLIC_URL=$origin
HOST=127.0.0.1
PORT=34871
OPERATOR_NAME=Checkpoint
HOSTING_COUNTRY=Pendiente de confirmar
PRIVACY_CONTACT=Pendiente de configurar
EOF
  chown root:checkpoint /etc/checkpoint/service.env
  chmod 0640 /etc/checkpoint/service.env
fi
cat > /etc/checkpoint/network.env << EOF
CHECKPOINT_LISTEN_ADDRESS=$listen_address
CHECKPOINT_INTERFACE=$interface
EOF
chmod 0640 /etc/checkpoint/network.env

# Bootstrap exposes only health over HTTP, never authentication.
cat > /etc/checkpoint/Caddyfile.bootstrap << EOF
{
    admin 127.0.0.1:2019
    default_bind $listen_address
    servers {
        protocols h1 h2
    }
}
http://$public_host {
    handle /health {
        reverse_proxy 127.0.0.1:34871
    }
    handle {
        respond "Checkpoint: preparando HTTPS." 503
    }
}
EOF
for mode in staging production; do
  if [[ $mode == staging ]]; then ca=https://acme-staging-v02.api.letsencrypt.org/directory; else ca=https://acme-v02.api.letsencrypt.org/directory; fi
  cat > "/etc/checkpoint/Caddyfile.$mode" << EOF
{
    admin 127.0.0.1:2019
    default_bind $listen_address
    auto_https disable_redirects
    servers {
        protocols h1 h2
    }
}
http://$public_host {
    redir $origin{uri} permanent
}
$origin {
    tls {
        issuer acme {
            dir $ca
            profile shortlived
            disable_tlsalpn_challenge
        }
    }
    reverse_proxy 127.0.0.1:34871
}
EOF
done
for config in /etc/checkpoint/Caddyfile.*; do
  chown root:checkpoint-proxy "$config"
  chmod 0640 "$config"
  /opt/checkpoint/bin/caddy adapt --config "$config" --adapter caddyfile > /dev/null
done
if test ! -e /etc/checkpoint/Caddyfile; then
  install -m 0640 -o root -g checkpoint-proxy /etc/checkpoint/Caddyfile.bootstrap /etc/checkpoint/Caddyfile
fi

cat > /etc/systemd/system/checkpoint.service << 'EOF'
[Unit]
Description=Checkpoint application service
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=checkpoint
Group=checkpoint
WorkingDirectory=/opt/checkpoint/current/server
EnvironmentFile=/etc/checkpoint/service.env
ExecStart=/usr/bin/node src/index.mjs
Restart=on-failure
RestartSec=5
TimeoutStopSec=30
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
StateDirectory=checkpoint
StateDirectoryMode=0750
UMask=0077
RestrictAddressFamilies=AF_INET AF_INET6 AF_UNIX

[Install]
WantedBy=multi-user.target
EOF
cat > /etc/systemd/system/checkpoint-firewall.service << 'EOF'
[Unit]
Description=Checkpoint scoped web firewall rule
After=network-online.target netfilter-persistent.service
Before=checkpoint-proxy.service

[Service]
Type=oneshot
RemainAfterExit=yes
EnvironmentFile=/etc/checkpoint/network.env
ExecStart=/opt/checkpoint/current/deploy/firewall.sh add
ExecStop=/opt/checkpoint/current/deploy/firewall.sh remove

[Install]
WantedBy=multi-user.target
EOF
cat > /etc/systemd/system/checkpoint-proxy.service << 'EOF'
[Unit]
Description=Checkpoint HTTPS reverse proxy
After=network-online.target checkpoint.service checkpoint-firewall.service
Wants=network-online.target checkpoint.service
Requires=checkpoint-firewall.service

[Service]
Type=simple
User=checkpoint-proxy
Group=checkpoint-proxy
Environment=HOME=/var/lib/checkpoint-proxy
Environment=XDG_DATA_HOME=/var/lib/checkpoint-proxy/data
Environment=XDG_CONFIG_HOME=/var/lib/checkpoint-proxy/config
ExecStart=/opt/checkpoint/bin/caddy run --config /etc/checkpoint/Caddyfile --adapter caddyfile
ExecReload=/opt/checkpoint/bin/caddy reload --config /etc/checkpoint/Caddyfile --adapter caddyfile
Restart=on-failure
RestartSec=5
TimeoutStopSec=30
AmbientCapabilities=CAP_NET_BIND_SERVICE
CapabilityBoundingSet=CAP_NET_BIND_SERVICE
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
StateDirectory=checkpoint-proxy
StateDirectoryMode=0750
UMask=0077
RestrictAddressFamilies=AF_INET AF_INET6 AF_UNIX AF_NETLINK

[Install]
WantedBy=multi-user.target
EOF

if test -e /opt/checkpoint/current && ! test -L /opt/checkpoint/current; then
  printf 'Existing current path is not a symlink; refusing replacement\n' >&2
  exit 1
fi
ln -s "$release_dir" /opt/checkpoint/current.next
mv -Tf /opt/checkpoint/current.next /opt/checkpoint/current
systemctl daemon-reload
systemctl enable checkpoint.service checkpoint-firewall.service checkpoint-proxy.service
systemctl restart checkpoint.service
systemctl restart checkpoint-firewall.service
systemctl restart checkpoint-proxy.service
for attempt in $(seq 1 15); do
  if curl -fsS --max-time 2 http://127.0.0.1:34871/health; then break; fi
  sleep 1
done
curl -fsS --max-time 5 "http://$listen_address/health" -H "Host: $public_host"
printf '\nDeployment ready. External HTTP/HTTPS validation still required.\n'
