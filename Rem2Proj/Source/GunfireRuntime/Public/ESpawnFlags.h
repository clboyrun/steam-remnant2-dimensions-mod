#pragma once
#include "CoreMinimal.h"
#include "ESpawnFlags.generated.h"

UENUM(BlueprintType)
enum class ESpawnFlags : uint8 {
    None,
    NavMesh,
    NavAccessible,
    OutOfSight = 4,
    InView = 8,
    InFieldOfView = 16,
};

