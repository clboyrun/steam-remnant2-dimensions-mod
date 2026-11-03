#pragma once
#include "CoreMinimal.h"
#include "Condition.h"
#include "EPrismSegmentCategory.h"
#include "PrismHasSegmentCondition.generated.h"

UCLASS(Blueprintable)
class REMNANT_API UPrismHasSegmentCondition : public UCondition {
    GENERATED_BODY()
public:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    FName SegmentNameID;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    int32 RequiredLevel;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    int32 RequiredRarity;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    EPrismSegmentCategory RequiredCategory;
    
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    bool RequiredComboSegment;
    
    UPrismHasSegmentCondition();

};

