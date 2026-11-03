#pragma once
#include "CoreMinimal.h"
#include "FeedData.generated.h"

USTRUCT(BlueprintType)
struct FFeedData {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, SaveGame, meta=(AllowPrivateAccess=true))
    FName RowName;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, SaveGame, meta=(AllowPrivateAccess=true))
    int32 FedLevel;
    
    REMNANT_API FFeedData();
};

