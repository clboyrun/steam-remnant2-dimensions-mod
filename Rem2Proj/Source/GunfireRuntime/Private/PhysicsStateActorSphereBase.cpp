#include "PhysicsStateActorSphereBase.h"
#include "Components/SphereComponent.h"

APhysicsStateActorSphereBase::APhysicsStateActorSphereBase(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<USphereComponent>(TEXT("PhysicsComponent"));
    this->bAutoSizeKinematicCollision = true;
    this->WeaponPhantomComponentName = TEXT("WeaponPhantomSphere");
    this->bAutoSizeWeaponPhantom = true;
    this->PhysicsBoundSets.AddDefaulted(1);
    this->PhysicsComponent = (UPrimitiveComponent*)RootComponent;
    this->WeaponPhantomSphere = CreateDefaultSubobject<USphereComponent>(TEXT("WeaponPhantomSphere"));
    this->KinematicCollisionSphere = CreateDefaultSubobject<USphereComponent>(TEXT("KinematicCollisionSphere"));
    this->WeaponPhantomSphere->SetupAttachment(RootComponent);
    this->KinematicCollisionSphere->SetupAttachment(RootComponent);
}

USphereComponent* APhysicsStateActorSphereBase::GetSphereComponent() const {
    return NULL;
}


