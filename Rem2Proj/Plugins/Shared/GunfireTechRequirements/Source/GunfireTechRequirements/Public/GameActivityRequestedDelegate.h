#pragma once
#include "CoreMinimal.h"
#include "FindSessionsCallbackProxy.h"
#include "GameActivityRequestedDelegate.generated.h"

UDELEGATE(BlueprintCallable) DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FGameActivityRequested, const FString&, ActivityId, const FBlueprintSessionResult&, SessionResult);

