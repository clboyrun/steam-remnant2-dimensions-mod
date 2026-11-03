#include "ProjectileSphere.h"
#include "Components/SphereComponent.h"

AProjectileSphere::AProjectileSphere(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USphereComponent>(TEXT("Collision"));
    this->Collision = (UShapeComponent*)RootComponent;
}


