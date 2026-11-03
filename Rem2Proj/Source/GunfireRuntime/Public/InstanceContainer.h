#pragma once
#include "CoreMinimal.h"
#include "InstancedStruct.h"
#include "InstanceContainer.generated.h"

USTRUCT(BlueprintType)
struct FInstanceContainer {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TArray<FInstancedStruct> InstanceArray;
    
    GUNFIRERUNTIME_API FInstanceContainer();
};

