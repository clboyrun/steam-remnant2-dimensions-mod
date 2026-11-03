#pragma once
#include "CoreMinimal.h"
#include "ETileFlags.generated.h"

UENUM()
enum class ETileFlags : uint8 {
    AllowSpawns = 1,
    AllowQuests,
    NoNavValidate = 4,
    AllFlags = 255,
};

