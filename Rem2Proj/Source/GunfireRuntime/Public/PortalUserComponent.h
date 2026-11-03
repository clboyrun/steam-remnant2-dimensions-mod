#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "Engine/NetSerialization.h"
#include "PortalUserComponent.generated.h"

class APortal;

UCLASS(Blueprintable, ClassGroup=Custom, meta=(BlueprintSpawnableComponent))
class GUNFIRERUNTIME_API UPortalUserComponent : public UActorComponent {
    GENERATED_BODY()
public:
    UPortalUserComponent(const FObjectInitializer& ObjectInitializer);

    UFUNCTION(BlueprintCallable, Reliable, Server)
    void ServerEnterPortal(APortal* EntryPortal, FVector_NetQuantize WorldLocation);
    
};

