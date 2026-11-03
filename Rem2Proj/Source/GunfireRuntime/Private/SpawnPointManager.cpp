#include "SpawnPointManager.h"
#include "Templates/SubclassOf.h"

USpawnPointManager::USpawnPointManager(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->NavMesh = NULL;
}

void USpawnPointManager::SpawnCharacter(const FSpawnEntry& Entry, AActor* Owner, const FVector& SpawnPos, bool Transient) {
}

AActor* USpawnPointManager::SpawnActorDeferred(UObject* WorldContextObject, TSubclassOf<AActor> ActorBP, const FTransform& Transform) {
    return NULL;
}

void USpawnPointManager::Spawn(const FBox& Region, const FSpawnList& SpawnList, ULevel* OwningLevel, bool Transient, TArray<AActor*>& SpawnedActors) {
}

USpawnPointManager* USpawnPointManager::GetInstance(UObject* WorldContextObject) {
    return NULL;
}

bool USpawnPointManager::FindSpawnPointOnNavMesh(AActor* RefActor, const FVector& RefPosition, FVector& OutPosition, float RadiusMin, float RadiusMax, bool InFOV) {
    return false;
}

bool USpawnPointManager::FindNearbySpawnPosition(AActor* Actor, FVector& OutPosition, float RadiusMin, float RadiusMax, bool InFOVOnly) {
    return false;
}

void USpawnPointManager::FinalizeSpawnActor(AActor* Actor, const FTransform& Transform) {
}


