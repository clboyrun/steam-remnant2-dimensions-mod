#pragma once
#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "LoadableItemAsset.generated.h"

class UObject;

USTRUCT(BlueprintType)
struct FLoadableItemAsset {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FName AssetName;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FSoftObjectPath Asset;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FSoftClassPath AssetClass;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, Transient, meta=(AllowPrivateAccess=true))
    UObject* Reference;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, Transient, meta=(AllowPrivateAccess=true))
    UClass* ClassReference;
    
    GUNFIRERUNTIME_API FLoadableItemAsset();
};

