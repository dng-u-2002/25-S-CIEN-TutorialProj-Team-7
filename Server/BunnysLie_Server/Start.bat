

icacls ssh-key-private.key /remove "NT AUTHORITY\Authenticated Users"
icacls ssh-key-private.key /remove Users
icacls ssh-key-private.key /remove Everyone
icacls ssh-key-private.key /grant %USERNAME%:R
icacls ssh-key-private.key

dotnet publish -c Release -o out
scp -i ssh-key-private.key -r .\out ubuntu@140.245.70.96:~/bunnyslie-server

ssh -i ssh-key-private.key ubuntu@140.245.70.96 ^
  "ls && sudo lsof -iUDP:9000 && sudo kill $(sudo lsof -t -iUDP:9000) && sudo iptables -I INPUT -j ACCEPT && cd bunnyslie-server/out && dotnet BunnysLie_Server.dll"

pause