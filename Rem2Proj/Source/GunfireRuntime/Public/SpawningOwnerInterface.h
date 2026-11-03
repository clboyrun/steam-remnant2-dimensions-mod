#pragma once
#include "CoreMinimal.h"
#include "UObject/Interface.h"
#include "SpawningOwnerInterface.generated.h"

class AActor;
class ULevel;

UINTERFACE(Blueprintable, MinimalAPI)
class USpawningOwnerInterface : public UInterface {
    GENERATED_BODY()
};

class ISpawningOwnerInterface : public IInterface {
    GENERATED_BODY()
public:
    UFUNCTION(BlueprintCallable, BlueprintNativeEvent)
    ULevel* GetWorldLevelForDrop(AActor* SourceActor);
    
};

