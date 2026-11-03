#include "ClimbingSurfaces.h"
#include "Components/SceneComponent.h"

AClimbingSurfaces::AClimbingSurfaces(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
}


