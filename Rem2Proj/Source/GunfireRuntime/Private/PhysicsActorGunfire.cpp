#include "PhysicsActorGunfire.h"

APhysicsActorGunfire::APhysicsActorGunfire(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->SleepThreshold = 15;
    this->VelocityNoiseFloor = 5.00f;
    this->LowVelocityEncountered = 0;
}

void APhysicsActorGunfire::SoundInstanceFinished(UPhysicsSoundInstance* SoundInstanceObject) {
}

void APhysicsActorGunfire::OnHitCallback(UPrimitiveComponent* HitComp, AActor* OtherActor, UPrimitiveComponent* OtherComp, FVector NormalImpulse, const FHitResult& Hit) {
}


