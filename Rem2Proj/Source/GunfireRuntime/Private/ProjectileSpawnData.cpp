#include "ProjectileSpawnData.h"

FProjectileSpawnData::FProjectileSpawnData() {
    this->CauseActor = NULL;
    this->InitialSpeed = 0.00f;
    this->IsSubProjectile = false;
    this->ArcType = EAutoArcType::None;
    this->FixedArcHeight = 0.00f;
}

