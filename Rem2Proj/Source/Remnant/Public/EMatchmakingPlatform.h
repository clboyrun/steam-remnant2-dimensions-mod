#pragma once
#include "CoreMinimal.h"
#include "EMatchmakingPlatform.generated.h"

UENUM(BlueprintType)
enum class EMatchmakingPlatform : uint8 {
    None = 0 UMETA(Hidden),
    Steam = 1,
    Epic,
    XBox = 4,
    Playstation = 8,
    Windows = 16,
};

