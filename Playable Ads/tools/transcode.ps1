param([string]$In, [string]$Out, [int]$Bitrate = 48000, [int]$Rate = 22050, [int]$Channels = 1, [double]$TrimStart = 0, [double]$TrimStop = 0)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
$asTaskAction = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncActionWithProgress`1' })[0]
function Await($op, [Type]$t) { $m = $asTaskGeneric.MakeGenericMethod($t); $task = $m.Invoke($null, @($op)); $task.Wait(-1) | Out-Null; $task.Result }
function AwaitAP($op, [Type]$p) { $m = $asTaskAction.MakeGenericMethod($p); $task = $m.Invoke($null, @($op)); $task.Wait(-1) | Out-Null }

[Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
[Windows.Storage.StorageFolder, Windows.Storage, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.Transcoding.MediaTranscoder, Windows.Media.Transcoding, ContentType = WindowsRuntime] | Out-Null
[Windows.Media.MediaProperties.MediaEncodingProfile, Windows.Media.MediaProperties, ContentType = WindowsRuntime] | Out-Null

$src = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($In)) ([Windows.Storage.StorageFile])
$dir = Split-Path $Out -Parent; $name = Split-Path $Out -Leaf
$folder = Await ([Windows.Storage.StorageFolder]::GetFolderFromPathAsync($dir)) ([Windows.Storage.StorageFolder])
$dst = Await ($folder.CreateFileAsync($name, [Windows.Storage.CreationCollisionOption]::ReplaceExisting)) ([Windows.Storage.StorageFile])

$profile = [Windows.Media.MediaProperties.MediaEncodingProfile]::CreateMp3([Windows.Media.MediaProperties.AudioEncodingQuality]::Low)
$profile.Audio.Bitrate = $Bitrate
$profile.Audio.SampleRate = $Rate
$profile.Audio.ChannelCount = $Channels
$tc = New-Object Windows.Media.Transcoding.MediaTranscoder
if ($TrimStart -gt 0) { $tc.TrimStartTime = [TimeSpan]::FromSeconds($TrimStart) }
if ($TrimStop -gt 0) { $tc.TrimStopTime = [TimeSpan]::FromSeconds($TrimStop) }
$prep = Await ($tc.PrepareFileTranscodeAsync($src, $dst, $profile)) ([Windows.Media.Transcoding.PrepareTranscodeResult])
if (-not $prep.CanTranscode) { throw "Cannot transcode: $($prep.FailureReason)" }
AwaitAP ($prep.TranscodeAsync()) ([double])
"OK $Out $((Get-Item $Out).Length)"
