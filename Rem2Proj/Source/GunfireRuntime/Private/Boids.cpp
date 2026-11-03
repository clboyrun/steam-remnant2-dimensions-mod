#include "Boids.h"
#include "Components/SceneComponent.h"

ABoids::ABoids(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("Default Scene Root"));
    this->SceneComponent = (USceneComponent*)RootComponent;
    this->BillboardComponent = NULL;
    this->World = NULL;
}

AActor* ABoids::SpawnBoid() {
    return NULL;
}

void ABoids::SetGoal(AActor* Goal) {
}

void ABoids::InitializeBoids() {
}

void ABoids::DestroyAllBoids() {
}

void ABoids::BoidSpawned_Implementation(AActor* Actor) {
}


