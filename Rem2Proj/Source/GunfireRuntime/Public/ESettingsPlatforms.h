#pragma once
#include "CoreMinimal.h"
#include "ESettingsPlatforms.generated.h"

UENUM()
enum class ESettingsPlatforms : int8 {
    PC = 1,
    XboxSeriesX,
    XboxSeriesS = 4,
    PS5 = 8,
    SteamDeck = 16,
};

