icacls ssh-key-private.key /remove "NT AUTHORITY\Authenticated Users"
icacls ssh-key-private.key /remove Users
icacls ssh-key-private.key /remove Everyone
icacls ssh-key-private.key /grant %USERNAME%:R
icacls ssh-key-private.key


ssh -i ssh-key-private.key ubuntu@140.245.70.96 ^
  "ls && lsof -i :9000 && sudo iptables -I INPUT -j ACCEPT && cd bunnyslie-server/out && dotnet BunnysLie_Server.dll"

pause