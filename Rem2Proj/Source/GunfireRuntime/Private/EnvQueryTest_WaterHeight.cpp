#include "EnvQueryTest_WaterHeight.h"

UEnvQueryTest_WaterHeight::UEnvQueryTest_WaterHeight() {
    this->TestPurpose = EEnvTestPurpose::Filter;
    this->FilterType = EEnvTestFilterType::Match;
    this->ScoringEquation = EEnvTestScoreEquation::Constant;
    this->WaterTraceChannel = ECC_WorldStatic;
    this->Compare = CompareOp::GreaterThan;
    this->WaterHeight = 10.00f;
}


