dotnet publish -c Release -o out


icacls ssh-key-private.key /inheritance:r
icacls ssh-key-private.key /remove "NT AUTHORITY\Authenticated Users"
icacls ssh-key-private.key /remove "BUILTIN\Users"
icacls ssh-key-private.key /remove "BUILTIN\Administrators"
icacls ssh-key-private.key /remove "NT AUTHORITY\SYSTEM"


icacls ssh-key-private.key /grant:r "%USERNAME%:R"


icacls ssh-key-private.key

scp -i ssh-key-private.key -r .\out ubuntu@140.245.70.96:~/bunnyslie-server


ssh -i ssh-key-private.key ubuntu@140.245.70.96 ^
  "sudo iptables -I INPUT -j ACCEPT; lsof -i :9000; sudo kill $(sudo lsof -t -iUDP:9000); cd bunnyslie-server/out && nohup dotnet BunnysLie_Server.dll"

pause