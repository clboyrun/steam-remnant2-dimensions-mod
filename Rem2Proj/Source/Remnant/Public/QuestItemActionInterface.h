#pragma once
#include "CoreMinimal.h"
#include "UObject/Interface.h"
#include "Templates/SubclassOf.h"
#include "QuestItemActionInterface.generated.h"

class UActionBase;
class UObject;

UINTERFACE(Blueprintable, MinimalAPI)
class UQuestItemActionInterface : public UInterface {
    GENERATED_BODY()
};

class IQuestItemActionInterface : public IInterface {
    GENERATED_BODY()
public:
    UFUNCTION(BlueprintCallable, BlueprintNativeEvent, meta=(WorldContext="WorldContextObject"))
    FName GetTargetZoneID(UObject* WorldContextObject) const;
    
    UFUNCTION(BlueprintCallable, BlueprintNativeEvent, meta=(WorldContext="WorldContextObject"))
    FName GetTargetActiveQuestID(UObject* WorldContextObject) const;
    
    UFUNCTION(BlueprintCallable, BlueprintNativeEvent, meta=(WorldContext="WorldContextObject"))
    TSubclassOf<UActionBase> GetGetActionToApply(UObject* WorldContextObject) const;
    
};

