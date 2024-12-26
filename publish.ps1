Param (
	[Parameter(Mandatory=$false)]
	[string]$Framework = 'net8.0',
	[Parameter(Mandatory)]
	[string]$version
)

$profiles = 'framework-dependent-win-x64', 'framework-dependent-linux-x64'

Function Publish-Proj
{
	Param (
		[Parameter(Mandatory)]
		[string]$DirName
	)

	try
	{
		Push-Location "./$DirName"
		$publishDir = './bin/Publish'

		foreach ($profile in $profiles) {
			Write-Host "Publishing $profile"
			if ($profile -eq 'framework-dependent') {
				Write-Host "Publishing $DirName framework dependent"
				dotnet publish -o "$publishDir/$profile/" --no-self-contained -c 'Release' -f $Framework -p:PublishSingleFile=false
			}
			elseif ($profile -like 'framework-dependent-*') {
				Write-Host "Publishing $DirName framework dependent specific"
				dotnet publish -o "$publishDir/$profile/" --no-self-contained -c 'Release' -f $Framework -r $profile.Substring('framework-dependent-'.Length) -p:PublishSingleFile=false
			}
			elseif ($DirName -eq 'ConsoleGames') {
				Write-Host "Publishing $DirName self contained trimmed"
				dotnet publish -o "$publishDir/$profile/" --sc -c 'Release' -f $Framework -r $profile -p:PublishTrimmed=true -p:TrimMode=full -p:SuppressTrimAnalysisWarnings=true
			}
			else {
				Write-Host "Publishing $DirName self contained partially trimmed"
				dotnet publish -o "$publishDir/$profile/" --sc -c 'Release' -f $Framework -r $profile -p:PublishTrimmed=true -p:TrimMode=partial
			}
		}

		Write-Host "Published $($profiles.Count) profile(s)"
	}
	finally
	{
		Pop-Location
	}
}

Publish-Proj 'MCeToJava'

$publishDir = 'Publish'

if (Test-Path -LiteralPath $publishDir) {
	Write-Host Cleaning
	Remove-Item -Path $publishDir -Recurse -Force > $null
}

Write-Host 'Copying files'

New-Item -Path . -Name $publishDir -ItemType "directory" -Force > $null

foreach ($profile in $profiles) {
	New-Item -Path $publishDir -Name $profile -ItemType "directory" -Force
	$outDir = "./$publishDir/$profile"

	Copy-Item -Path "./MCeToJava/bin/Publish/$profile/*" -Destination $outDir -Recurse
}

Write-Host 'Compressing folders'

foreach ($profile in $profiles) {
	$fromDir = "./$publishDir/$profile/*"
	$toFile = "./$publishDir/MCeToJava_$($profile)_$version.zip"
	Write-Host "Comressing $fromDir to $toFile"
	
	Compress-Archive -Path $fromDir -CompressionLevel 'Optimal' -DestinationPath $toFile -Force
}

Write-Host 'Done'