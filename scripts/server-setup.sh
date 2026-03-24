#!/usr/bin/env bash
set -euo pipefail

###############################################################################
# BaGetter — VPS security hardening & Docker setup
#
# Tested on: Debian 12 / Ubuntu 24.04
# Run as root: curl -sSL <url> | bash
# Or:          chmod +x server-setup.sh && sudo ./server-setup.sh
###############################################################################

echo "==> Updating system packages..."
apt-get update -qq && apt-get upgrade -y -qq

# ─── Essential packages ─────────────────────────────────────────────────────
echo "==> Installing essential packages..."
apt-get install -y -qq \
  ufw \
  fail2ban \
  unattended-upgrades \
  apt-listchanges \
  curl \
  wget \
  git \
  ca-certificates \
  gnupg

# ─── Firewall (UFW) ─────────────────────────────────────────────────────────
echo "==> Configuring firewall..."
ufw default deny incoming
ufw default allow outgoing
ufw allow OpenSSH
ufw allow 80/tcp    # HTTP  (Caddy / Let's Encrypt)
ufw allow 443/tcp   # HTTPS (Caddy)
ufw --force enable
echo "    Firewall enabled: SSH, HTTP, HTTPS only."

# ─── SSH hardening ───────────────────────────────────────────────────────────
echo "==> Hardening SSH..."
SSHD_CONFIG="/etc/ssh/sshd_config"
cp "$SSHD_CONFIG" "$SSHD_CONFIG.bak.$(date +%s)"

# Disable root login and password auth (ensure SSH key access is set up first)
sed -i 's/^#\?PermitRootLogin .*/PermitRootLogin no/' "$SSHD_CONFIG"
sed -i 's/^#\?PasswordAuthentication .*/PasswordAuthentication no/' "$SSHD_CONFIG"
sed -i 's/^#\?ChallengeResponseAuthentication .*/ChallengeResponseAuthentication no/' "$SSHD_CONFIG"
sed -i 's/^#\?X11Forwarding .*/X11Forwarding no/' "$SSHD_CONFIG"
sed -i 's/^#\?MaxAuthTries .*/MaxAuthTries 3/' "$SSHD_CONFIG"

systemctl restart sshd
echo "    SSH: root login disabled, password auth disabled, max 3 auth tries."

# ─── Fail2ban ────────────────────────────────────────────────────────────────
echo "==> Configuring fail2ban..."
cat > /etc/fail2ban/jail.local <<'JAIL'
[DEFAULT]
bantime  = 1h
findtime = 10m
maxretry = 5

[sshd]
enabled = true
port    = ssh
filter  = sshd
logpath = /var/log/auth.log
JAIL

systemctl enable fail2ban
systemctl restart fail2ban
echo "    Fail2ban: SSH jail enabled (5 retries → 1h ban)."

# ─── Automatic security updates ─────────────────────────────────────────────
echo "==> Enabling automatic security updates..."
cat > /etc/apt/apt.conf.d/20auto-upgrades <<'AUTOUPG'
APT::Periodic::Update-Package-Lists "1";
APT::Periodic::Unattended-Upgrade "1";
APT::Periodic::AutocleanInterval "7";
AUTOUPG
echo "    Unattended upgrades enabled."

# ─── Docker Engine ───────────────────────────────────────────────────────────
echo "==> Installing Docker Engine..."
if ! command -v docker &> /dev/null; then
  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/$(. /etc/os-release && echo "$ID")/gpg \
    | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
    https://download.docker.com/linux/$(. /etc/os-release && echo "$ID") \
    $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
    > /etc/apt/sources.list.d/docker.list

  apt-get update -qq
  apt-get install -y -qq docker-ce docker-ce-cli containerd.io docker-compose-plugin
  echo "    Docker installed."
else
  echo "    Docker already installed, skipping."
fi

# ─── Deploy user ─────────────────────────────────────────────────────────────
echo "==> Creating deploy user..."
if ! id "deploy" &>/dev/null; then
  useradd -m -s /bin/bash -G docker deploy
  mkdir -p /home/deploy/.ssh
  # Copy root's authorized keys so the same SSH key works
  cp /root/.ssh/authorized_keys /home/deploy/.ssh/authorized_keys 2>/dev/null || true
  chown -R deploy:deploy /home/deploy/.ssh
  chmod 700 /home/deploy/.ssh
  chmod 600 /home/deploy/.ssh/authorized_keys 2>/dev/null || true
  echo "    User 'deploy' created with Docker access."
else
  usermod -aG docker deploy
  echo "    User 'deploy' already exists, added to docker group."
fi

# ─── Summary ─────────────────────────────────────────────────────────────────
echo ""
echo "============================================================"
echo "  Server setup complete!"
echo "============================================================"
echo ""
echo "  Next steps:"
echo "    1. Set up SSH key for 'deploy' user if not already done"
echo "    2. Log in as deploy:  ssh deploy@<server-ip>"
echo "    3. Clone the repo:   git clone <repo-url> ~/BaGetter"
echo "    4. Create .env:      cd ~/BaGetter && cp .env.example .env"
echo "    5. Edit .env with strong passwords"
echo "    6. Start the stack:"
echo "       docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build"
echo "    7. Verify:           curl https://packages.dev.coderise.fr/health"
echo ""
echo "  DNS: point packages.dev.coderise.fr A record to this server's IP"
echo ""
