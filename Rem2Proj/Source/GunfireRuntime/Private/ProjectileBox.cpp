#include "ProjectileBox.h"
#include "Components/BoxComponent.h"

AProjectileBox::AProjectileBox(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<UBoxComponent>(TEXT("Collision"));
    this->Collision = (UShapeComponent*)RootComponent;
}


