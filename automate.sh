#!/bin/zsh
cd ./ServerApp
zip server.zip server.js
cd ../
ScriptId=$(aws gamelift create-script --name "test" --zip-file fileb://ServerApp/server.zip | jq -r '.Script.ScriptId')

#### CFnスタック作成
StackId=$(aws cloudformation create-stack --stack-name UnityGameLiftRealtimeSample --template-body file://AWSResources/resources.yaml --parameters ParameterKey=GameLiftScriptId,ParameterValue=$ScriptId --capabilities CAPABILITY_IAM | jq -r '.StackId')


## StackIdからStacNameのみ抜き出す処理 (UnityGameLiftRealtimeSample2　を抜き出す処理　：　要実装)
StackName=$(echo $StackId | tr '/' '\n'| head -2 | tail -1)


## CREATE_IN_PROGRESSじゃなくなったかどうかの確認 (CREATE_COMPLETE　になる)
response=CREATE_IN_PROGRESS
while [ $response = 'CREATE_IN_PROGRESS' ]
do
    response=$(aws cloudformation describe-stacks --stack-name $StackName | jq -r '.Stacks[].StackStatus')
    echo $response
    sleep 180
done

#### IAMアクセスキー発行
IAMUserName=$(aws cloudformation describe-stack-resources --stack-name $StackName | jq -r '.StackResources[]|select(.ResourceType == "AWS::IAM::User")|.PhysicalResourceId')

Result=$(aws iam create-access-key --user-name $IAMUserName)

AccessKeyId=$(echo $Result | jq -r '.AccessKey.AccessKeyId')
SecretAccessKey=$(echo $Result | jq -r '.AccessKey.SecretAccessKey')

GameLiftAlias=$(aws cloudformation describe-stack-resources --stack-name $StackName | jq -r '.StackResources[]|select(.ResourceType =="AWS::GameLift::Alias")|.PhysicalResourceId')

## シークレット表示
echo $AccessKeyId
echo $SecretAccessKey
echo $GameLiftAlias