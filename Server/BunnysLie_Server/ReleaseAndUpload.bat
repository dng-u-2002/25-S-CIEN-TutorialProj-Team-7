dotnet publish -c Release -o out
scp -i ssh-key-private.key -r .\out ubuntu@140.245.70.96:~/bunnyslie-server