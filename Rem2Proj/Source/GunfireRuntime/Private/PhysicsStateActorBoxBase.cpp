#include "PhysicsStateActorBoxBase.h"
#include "Components/BoxComponent.h"

APhysicsStateActorBoxBase::APhysicsStateActorBoxBase(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<UBoxComponent>(TEXT("PhysicsComponent"));
    this->bAutoSizeKinematicCollision = true;
    this->WeaponPhantomComponentName = TEXT("WeaponPhantomBox");
    this->bAutoSizeWeaponPhantom = true;
    this->PhysicsBoundSets.AddDefaulted(1);
    this->PhysicsComponent = (UPrimitiveComponent*)RootComponent;
    this->WeaponPhantomBox = CreateDefaultSubobject<UBoxComponent>(TEXT("WeaponPhantomBox"));
    this->KinematicCollisionBox = CreateDefaultSubobject<UBoxComponent>(TEXT("KinematicCollisionBox"));
    this->WeaponPhantomBox->SetupAttachment(RootComponent);
    this->KinematicCollisionBox->SetupAttachment(RootComponent);
}

UBoxComponent* APhysicsStateActorBoxBase::GetBoxComponent() const {
    return NULL;
}


