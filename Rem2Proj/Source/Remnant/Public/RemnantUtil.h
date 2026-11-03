#pragma once
#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "UObject/NoExportTypes.h"
#include "ZoneLinkInfo.h"
#include "RemnantUtil.generated.h"

class AActor;
class AZoneActor;
class ULevel;
class UMapGenComponent;

UCLASS(Blueprintable)
class REMNANT_API URemnantUtil : public UObject {
    GENERATED_BODY()
public:
    URemnantUtil();

    UFUNCTION(BlueprintCallable, BlueprintPure)
    static bool IsZoneLinkValid(const FZoneLinkInfo& ZoneLink);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static bool IsValueEncodedReleaseHours(int32 Value, int32 SigDigits);
    
    UFUNCTION(BlueprintCallable, BlueprintPure, meta=(WorldContext="WorldContextObject"))
    static bool IsGameSessionReady(const UObject* WorldContextObject);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static bool IsGameSeedOverridden();
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static AActor* GetZoneLinkActor(const FZoneLinkInfo& ZoneLink);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static FTimespan GetYearsSinceStudioOpen();
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static FTimespan GetTimeSinceRelease(int32 AdditionalHourDelta);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static ULevel* GetRemnantMapGenLevelForActor(AActor* Actor);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static UMapGenComponent* GetRemnantMapGenComponentForActor(AActor* Actor);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static int32 GetGameSeed();
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static int32 GetEncodingForTimespan(FTimespan Timespan, int32 SigDigits);
    
    UFUNCTION(BlueprintCallable, BlueprintPure, meta=(WorldContext="WorldContextObject"))
    static bool GetBaseZoneLinkByLabel(const UObject* WorldContextObject, const FString& Label, FZoneLinkInfo& OutZoneLink);
    
    UFUNCTION(BlueprintCallable, BlueprintPure, meta=(WorldContext="WorldContextObject"))
    static bool GetBaseZoneLink(const UObject* WorldContextObject, FZoneLinkInfo& OutZoneLink);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static int32 GetBaseZoneID();
    
    UFUNCTION(BlueprintCallable, BlueprintPure, meta=(WorldContext="WorldContextObject"))
    static AZoneActor* GetBaseZoneActor(const UObject* WorldContextObject);
    
    UFUNCTION(BlueprintCallable, BlueprintPure)
    static int32 EncodeHoursSinceRelease(int32 SigDigits);
    
    UFUNCTION(BlueprintCallable, BlueprintPure, meta=(WorldContext="WorldContextObject"))
    static bool CanPlayerExitGame(const UObject* WorldContextObject);
    
};

