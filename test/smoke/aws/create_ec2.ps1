$p1 = new-object Amazon.CloudFormation.Model.Parameter -Property @{ParameterKey="KeyName"; ParameterValue="$env:KEY_NAME"}
$p2 = new-object Amazon.CloudFormation.Model.Parameter -Property @{ParameterKey="GitBranch"; ParameterValue="$env:CIRCLE_BRANCH"}
$p3 = new-object Amazon.CloudFormation.Model.Parameter -Property @{ParameterKey="S3BucketName"; ParameterValue="$env:S3_BUCKET"}
$p4 = new-object Amazon.CloudFormation.Model.Parameter -Property @{ParameterKey="ImageId"; ParameterValue="$env:IMAGE_ID"}
$p5 = new-object Amazon.CloudFormation.Model.Parameter -Property @{ParameterKey="BuildNumber"; ParameterValue="$env:CIRCLE_BUILD_NUM"}
$params = @($p1,$p2,$p3,$p4,$p5)

# validate cloudformation template
$content = Get-Content -Path .\template.json -Raw
Test-CFNTemplate -TemplateBody $content

# create stack
New-CFNStack -StackName guardian-stack-$env:CIRCLE_BUILD_NUM -TemplateBody $content -Capability CAPABILITY_IAM -Parameter $params

# wait stack to complete
Wait-CFNStack -StackName guardian-stack-$env:CIRCLE_BUILD_NUM
