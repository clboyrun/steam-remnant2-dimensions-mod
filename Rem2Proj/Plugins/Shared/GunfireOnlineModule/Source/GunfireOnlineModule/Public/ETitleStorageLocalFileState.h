#pragma once
#include "CoreMinimal.h"
#include "ETitleStorageLocalFileState.generated.h"

UENUM(BlueprintType)
enum class ETitleStorageLocalFileState : uint8 {
    HeaderOnly,
    Downloading,
    Downloaded,
};

