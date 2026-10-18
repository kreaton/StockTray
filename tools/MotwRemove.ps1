 $files = Get-ChildItem $psscriptroot -Recurse
 $files
 
 # Show MOTW status for all files
 foreach ($file in $files) {
     if (Test-Path $file -PathType Leaf) {
         Get-Item $file.FullName | Select-Object FullName, @{Name="Zone.Identifier";Expression={(Get-Item $file.FullName -Stream "Zone.Identifier" -ErrorAction SilentlyContinue).Value}}
     }
 }

 
#  # Remove the Mark of the Web (MOTW) from all files recursively in the script root directory
#  foreach ($file in $files) {
#      if (Test-Path $file -PathType Leaf) {
#          Unblock-File -Path $file.FullName
#      }
#  }
