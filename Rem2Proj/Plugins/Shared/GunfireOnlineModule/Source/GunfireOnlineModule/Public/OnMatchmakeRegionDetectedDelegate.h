#pragma once
#include "CoreMinimal.h"
#include "EMatchmakingRegions.h"
#include "OnMatchmakeRegionDetectedDelegate.generated.h"

UDELEGATE(BlueprintCallable) DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnMatchmakeRegionDetected, EMatchmakingRegions, Region);

