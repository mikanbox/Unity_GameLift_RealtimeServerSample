

## 検証環境
Unity : 2019 4.14f1 Personal
macOS : Catalina

## Setup手順概要
1. AWS側のリソースの作成
2. Unity側のコード修正と起動


### AWS Resourcesの作成
#### Scriptの作成
```
cd ./ServerApp
zip server.zip server.js
cd ../
ScriptId=$(aws gamelift create-script --name "test" --zip-file fileb://ServerApp/server.zip | jq -r '.Script.ScriptId')
```

#### CFnスタック作成
```
StackId=$(aws cloudformation create-stack --stack-name UnityGameLiftRealtimeSample --template-body file://AWSResources/resources.yaml --parameters ParameterKey=GameLiftScriptId,ParameterValue=$ScriptId --capabilities CAPABILITY_IAM | jq -r '.StackId')
```

## StackIdからStacNameのみ抜き出す処理 (UnityGameLiftRealtimeSample2　を抜き出す処理)
```
StackName=$(echo $StackId | tr '/' '\n'| head -2 | tail -1)
```

## CREATE_IN_PROGRESSじゃなくなったかどうかの確認 (CREATE_COMPLETE　になる)
```
response=CREATE_IN_PROGRESS
while [ $response = 'CREATE_IN_PROGRESS' ]
do
    response=$(aws cloudformation describe-stacks --stack-name $StackName | jq -r '.Stacks[].StackStatus')
    echo $response
    sleep 180
done
```

#### IAMアクセスキー発行
```
IAMUserName=$(aws cloudformation describe-stack-resources --stack-name $StackName | jq -r '.StackResources[]|select(.ResourceType == "AWS::IAM::User")|.PhysicalResourceId')

Result=$(aws iam create-access-key --user-name $IAMUserName)

AccessKeyId=$(echo $Result | jq -r '.AccessKey.AccessKeyId')
SecretAccessKey=$(echo $Result | jq -r '.AccessKey.SecretAccessKey')
```

#### エイリアスId取得
```
GameLiftAlias=$(aws cloudformation describe-stack-resources --stack-name $StackName | jq -r '.StackResources[]|select(.ResourceType =="AWS::GameLift::Alias")|.PhysicalResourceId')
```

以下結果を RoomMgr.cs に記載
```
echo $AccessKeyId
echo $SecretAccessKey
echo $GameLiftAlias
```

記載先 RoomMgr.cs 
```
    string accessKeyId = "";
    string secretAccessKey = "";
    string gameLiftAliasId = "";
```

## Unityビルドと実行
SampleSceneを開く
ビルド後、アプリケーションフォルダにて実行
OSX
open hoge.app -n
open hoge.app -n


## Scriptのアップデート
cd ./ServerApp
zip server.zip server.js
cd ../
aws gamelift  update-script --zip-file fileb://ServerApp/server.zip --script-id $ScriptId


## 片付け
### RealTimeClientWrapper.cs 29行目 ~ 31行目から登録したキーの削除

### CFnスタックの終了
### アップロードしたGameLift Scriptの削除



## Shell Script化
```
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


echo $AccessKeyId
echo $SecretAccessKey
echo $GameLiftAlias
```
