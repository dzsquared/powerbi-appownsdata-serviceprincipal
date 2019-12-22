import-module Az

# Required to sign in as a tenant admin
Connect-AzAccount

# Create a new AAD web application
$app = Get-AzADApplication -ApplicationId 12345678-1234-1234-1234-123456789012

# Creates a service principal
$sp = New-AzADServicePrincipal -ApplicationId $app.ApplicationId -DisplayName "PowerBI_ServicePrincipal"

# Get the service principal key.
$key = New-AzADSpCredential -ObjectId $sp.ObjectId

# Create an AAD security group
$group = New-AzADGroup -DisplayName "PowerBI_Security" -MailNickName notSet

# Add the service principal to the group
Add-AzADGroupMember -TargetGroupObjectId $($group.ObjectId) -MemberObjectId $($sp.ObjectId)