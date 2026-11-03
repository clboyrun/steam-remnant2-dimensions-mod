#pragma once
#include "CoreMinimal.h"
#include "EQuestMode.generated.h"

UENUM(BlueprintType)
enum class EQuestMode : uint8 {
    Campaign,
    Adventure,
    BossRush,
    Survival,
    None,
};

