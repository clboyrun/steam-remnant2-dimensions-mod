#pragma once
#include "CoreMinimal.h"
#include "EMatchmakingRegions.generated.h"

UENUM(BlueprintType)
enum class EMatchmakingRegions : uint8 {
    UNKNOWN,
    ASIA,
    BR,
    EU,
    ME,
    OCE,
    NA,
};

