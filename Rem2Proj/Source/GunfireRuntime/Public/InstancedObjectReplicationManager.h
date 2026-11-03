#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "InstanceContainer.h"
#include "ProjectileSpawnData.h"
#include "Templates/SubclassOf.h"
#include "InstancedObjectReplicationManager.generated.h"

class AActor;
class UInstancedObjectReplicationManager;
class UObject;

UCLASS(Blueprintable, ClassGroup=Custom, meta=(BlueprintSpawnableComponent))
class GUNFIRERUNTIME_API UInstancedObjectReplicationManager : public UActorComponent {
    GENERATED_BODY()
public:
protected:
    UPROPERTY(BlueprintReadWrite, EditAnywhere, meta=(AllowPrivateAccess=true))
    TMap<TSubclassOf<AActor>, FInstanceContainer> ClassDataMap;
    
public:
    UInstancedObjectReplicationManager(const FObjectInitializer& ObjectInitializer);

    UFUNCTION(BlueprintCallable, NetMulticast, Reliable)
    void ReplicateCurrentObjectInstances(const TArray<TSubclassOf<AActor>>& SubclassArray, const TArray<FInstanceContainer>& ContainerArray);
    
    UFUNCTION(BlueprintCallable, meta=(WorldContext="WorldContextObject"))
    static UInstancedObjectReplicationManager* GetInstancedObjectReplicationManager(UObject* WorldContextObject);
    
    UFUNCTION(BlueprintCallable)
    void ClearDataMap();
    
    UFUNCTION(BlueprintCallable)
    void AddProjectileInstance(TSubclassOf<AActor> ProjectileClass, UPARAM(Ref) FProjectileSpawnData& Data);
    
};

