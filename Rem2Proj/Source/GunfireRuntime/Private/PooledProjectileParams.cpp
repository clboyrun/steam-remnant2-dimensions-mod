#include "PooledProjectileParams.h"

FPooledProjectileParams::FPooledProjectileParams() {
    this->ProjectileClass = NULL;
    this->NewCause = NULL;
    this->PoolSize = 0;
    this->bStartWithNetworkingActive = false;
}

