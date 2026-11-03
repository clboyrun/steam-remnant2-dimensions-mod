#pragma once
#include "CoreMinimal.h"
#include "Engine/DataTable.h"
#include "EQuestMode.h"
#include "SpawnTableOverrideParams.generated.h"

class ARemnantQuest;
class USpawnTable;

USTRUCT(BlueprintType)
struct REMNANT_API FSpawnTableOverrideParams : public FTableRowBase {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TSoftObjectPtr<USpawnTable> InitialTable;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TSoftObjectPtr<USpawnTable> OverrideTable;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TArray<FName> SpawnTags;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    bool bCheckOverrideMode;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TArray<EQuestMode> OverrideModes;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    bool bCheckOverrideRootQuest;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TSoftClassPtr<ARemnantQuest> OverrideRootQuest;
    
    FSpawnTableOverrideParams();
};

