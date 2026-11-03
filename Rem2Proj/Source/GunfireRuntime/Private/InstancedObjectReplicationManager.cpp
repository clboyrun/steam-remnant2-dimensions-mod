#include "InstancedObjectReplicationManager.h"
#include "Templates/SubclassOf.h"

UInstancedObjectReplicationManager::UInstancedObjectReplicationManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
}

void UInstancedObjectReplicationManager::ReplicateCurrentObjectInstances_Implementation(const TArray<TSubclassOf<AActor>>& SubclassArray, const TArray<FInstanceContainer>& ContainerArray) {
}

UInstancedObjectReplicationManager* UInstancedObjectReplicationManager::GetInstancedObjectReplicationManager(UObject* WorldContextObject) {
    return NULL;
}

void UInstancedObjectReplicationManager::ClearDataMap() {
}

void UInstancedObjectReplicationManager::AddProjectileInstance(TSubclassOf<AActor> ProjectileClass, FProjectileSpawnData& Data) {
}


