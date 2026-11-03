#include "ClimbingSurfaceModifier.h"
#include "Components/BoxComponent.h"

AClimbingSurfaceModifier::AClimbingSurfaceModifier(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<UBoxComponent>(TEXT("ModifierBox"));
    this->Modifier = EClimbingSurfaceModifierType::NoSurfaces;
    this->ModifierBox = (UBoxComponent*)RootComponent;
}

bool AClimbingSurfaceModifier::IsPointInModifier(const FVector& Point) const {
    return false;
}


