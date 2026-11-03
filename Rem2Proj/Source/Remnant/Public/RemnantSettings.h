#pragma once
#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "UObject/Object.h"
#include "Engine/EngineBaseTypes.h"
#include "CursorSettings.h"
#include "EPlayerTravelState.h"
#include "RemnantSettings.generated.h"

class UDamageClass;
class UDataTable;

UCLASS(Blueprintable, DefaultConfig, Config=Engine)
class REMNANT_API URemnantSettings : public UObject {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    float ModPowerSplashScalar;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    float ModPowerSplashFromOtherModScalar;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    float BonusModPower;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    TSoftClassPtr<UDamageClass> ModDamageClass;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    bool bAnalogCursorOverridesMouseLock;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    EMouseLockMode AnalogCursorMouseLockMode;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    FCursorSettings AnalogCursorMovementSettings;
    
    UPROPERTY(AdvancedDisplay, BlueprintReadWrite, Config, EditAnywhere, meta=(AllowPrivateAccess=true))
    TArray<FDirectoryPath> QuestDirectoryBlacklist;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    float PingDistance;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    TMap<EPlayerTravelState, float> TravelStateTimeouts;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    TArray<TSoftObjectPtr<UDataTable>> PrismSegmentDataTables;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    FDateTime ReleaseDate;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    FDateTime StudioDate;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, GlobalConfig, meta=(AllowPrivateAccess=true))
    int32 LanyardNumber;
    
    URemnantSettings();

};

