#pragma once
#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "LevelUtils.generated.h"

class ULevelStreaming;

UCLASS(Blueprintable)
class GUNFIRERUNTIME_API ULevelUtils : public UObject {
    GENERATED_BODY()
public:
    ULevelUtils();

    UFUNCTION(BlueprintCallable)
    static TArray<FString> GetAllSublevelPaths(const ULevelStreaming* StreamingLevel);
    
};

