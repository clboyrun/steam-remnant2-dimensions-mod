#include "AIGoalActor.h"
#include "Components/SceneComponent.h"

AAIGoalActor::AAIGoalActor(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
    this->Pawn = NULL;
}


