#include "PhysicsStateActorStaticMeshBase.h"
#include "Components/StaticMeshComponent.h"

APhysicsStateActorStaticMeshBase::APhysicsStateActorStaticMeshBase(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->RootComponent = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("PhysicsComponent"));
    this->bAutoSizeKinematicCollision = true;
    this->WeaponPhantomComponentName = TEXT("WeaponPhantomMesh");
    this->bAutoSizeWeaponPhantom = true;
    this->PhysicsBoundSets.AddDefaulted(1);
    this->PhysicsComponent = (UPrimitiveComponent*)RootComponent;
    this->WeaponPhantomMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("WeaponPhantomMesh"));
    this->KinematicCollisionMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("KinematicCollisionMesh"));
    this->WeaponPhantomMesh->SetupAttachment(RootComponent);
    this->KinematicCollisionMesh->SetupAttachment(RootComponent);
}

UStaticMeshComponent* APhysicsStateActorStaticMeshBase::GetStaticMeshComponent() const {
    return NULL;
}


