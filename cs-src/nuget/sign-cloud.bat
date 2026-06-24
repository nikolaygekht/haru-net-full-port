@ECHO off
forfiles /m *.nupkg /c "cmd /c nuget sign -CertificateFingerprint %CERTUM_CERTIFICATE% -Timestamper http://timestamp.digicert.com @path"
