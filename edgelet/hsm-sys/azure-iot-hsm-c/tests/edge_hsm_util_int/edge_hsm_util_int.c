// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "testrunnerswitcher.h"
#include "azure_c_shared_utility/gballoc.h"
#include "hsm_log.h"

// //#############################################################################
// // Declare and enable MOCK definitions
// //#############################################################################

#define TEST_FILE_ALPHA "test_alpha.txt"
#define TEST_FILE_NUMERIC "test_numeric.txt"
#define TEST_FILE_BAD "test_bad.txt"
#define TEST_FILE_EMPTY "test_empty.txt"
#define TEST_WRITE_FILE "test_write_data.txt"
#define TEST_WRITE_FILE_FOR_DELETE "test_write_data_del.txt"

//#############################################################################
// Interface(s) under test
//#############################################################################

#include "hsm_utils.h"

//#############################################################################
// Test defines and data
//#############################################################################

static TEST_MUTEX_HANDLE g_testByTest;
static TEST_MUTEX_HANDLE g_dllByDll;

//#############################################################################
// Test helpers
//#############################################################################
int test_helper_write_data_to_file
(
    const char* file_name,
    const unsigned char* input_data,
    size_t input_data_size
)
{
    FILE *file_handle;
    int result;
    if ((file_handle = fopen(file_name, "w")) == NULL)
    {
        LOG_ERROR("Could not open file for write %s", file_name);
        result = __LINE__;
    }
    else
    {
        result = 0;
        if (input_data != NULL)
        {
            size_t num_bytes_written = fwrite(input_data, 1, input_data_size, file_handle);
            if (num_bytes_written != input_data_size)
            {
                LOG_ERROR("File write failed for file %s", file_name);
                result = __FAILURE__;
            }
        }
    }

    if (file_handle != NULL)
    {
        fclose(file_handle);
    }

    return result;
}

void delete_file_if_exists(const char* file_name)
{
    (void)remove(file_name);
}

//#############################################################################
// Test cases
//#############################################################################

BEGIN_TEST_SUITE(edge_hsm_util_int_tests)

        TEST_SUITE_INITIALIZE(TestClassInitialize)
        {
            TEST_INITIALIZE_MEMORY_DEBUG(g_dllByDll);
            g_testByTest = TEST_MUTEX_CREATE();
            ASSERT_IS_NOT_NULL(g_testByTest);

            char alpha[] = "ABCD";
            ASSERT_ARE_EQUAL(int, 0, test_helper_write_data_to_file(TEST_FILE_ALPHA, alpha, strlen(alpha)));
            unsigned char numeric[] = {'1', '2', '3', '4'};
            ASSERT_ARE_EQUAL(int, 0, test_helper_write_data_to_file(TEST_FILE_NUMERIC, numeric, sizeof(numeric)));
            ASSERT_ARE_EQUAL(int, 0, test_helper_write_data_to_file(TEST_FILE_EMPTY, NULL, 0));
        }

        TEST_SUITE_CLEANUP(TestClassCleanup)
        {
            delete_file_if_exists(TEST_FILE_ALPHA);
            delete_file_if_exists(TEST_FILE_NUMERIC);
            delete_file_if_exists(TEST_FILE_EMPTY);

            TEST_MUTEX_DESTROY(g_testByTest);
            TEST_DEINITIALIZE_MEMORY_DEBUG(g_dllByDll);
        }

        TEST_FUNCTION_INITIALIZE(TestMethodInitialize)
        {
            if (TEST_MUTEX_ACQUIRE(g_testByTest))
            {
                ASSERT_FAIL("Mutex is ABANDONED. Failure in test framework.");
            }
        }

        TEST_FUNCTION_CLEANUP(TestMethodCleanup)
        {
            TEST_MUTEX_RELEASE(g_testByTest);
        }

        TEST_FUNCTION(read_file_into_cstring_smoke)
        {
            // arrange
            char *expected_string = "ABCD";
            size_t expected_string_size = 5;

            // act
            size_t output_size = 0;
            char *output_string = read_file_into_cstring(TEST_FILE_ALPHA, &output_size);

            // assert
            ASSERT_IS_NOT_NULL(output_string);
            int cmp_result = strcmp(expected_string, output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, cmp_result, "Line:" TOSTRING(__LINE__));
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, expected_string_size, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
            free(output_string);
        }

        TEST_FUNCTION(read_file_into_cstring_non_existant_file_returns_null)
        {
            // arrange
            size_t output_size = 100;

            // act
            char *output_string = read_file_into_cstring(TEST_FILE_BAD, &output_size);

            // assert
            ASSERT_IS_NULL(output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, 0, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(read_file_into_cstring_empty_file_returns_null)
        {
            // arrange
            size_t output_size = 100;

            // act
            char *output_string = read_file_into_cstring(TEST_FILE_EMPTY, &output_size);

            // assert
            ASSERT_IS_NULL(output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, 0, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(read_file_into_cstring_invalid_params_returns_null)
        {
            // arrange
            size_t output_size;
            unsigned char *output_string;

            // act, assert
            output_size = 100;
            output_string = read_file_into_cstring(NULL, &output_size);
            ASSERT_IS_NULL(output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, 0, output_size, "Line:" TOSTRING(__LINE__));

            // act, assert
            output_size = 100;
            output_string = read_file_into_cstring("", &output_size);
            ASSERT_IS_NULL(output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, 0, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(read_file_into_cbuffer_smoke)
        {
            // arrange
            unsigned char expected_buffer[] = {'1', '2', '3', '4'};
            size_t expected_buffer_size = 4;

            // act
            size_t output_size = 0;
            unsigned char *output_buffer = read_file_into_buffer(TEST_FILE_NUMERIC, &output_size);

            // assert
            ASSERT_IS_NOT_NULL(output_buffer);
            int cmp_result = memcmp(expected_buffer, output_buffer, expected_buffer_size);
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, cmp_result, "Line:" TOSTRING(__LINE__));
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, expected_buffer_size, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
            free(output_buffer);
        }

        TEST_FUNCTION(read_file_into_cbuffer_invalid_params_returns_null)
        {
            // arrange
            size_t output_size;
            unsigned char *output_buffer;

            // act, assert
            output_size = 100;
            output_buffer = read_file_into_buffer(NULL, &output_size);
            ASSERT_IS_NULL(output_buffer);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, 0, output_size, "Line:" TOSTRING(__LINE__));

            // act, assert
            output_size = 100;
            output_buffer = read_file_into_buffer("", &output_size);
            ASSERT_IS_NULL(output_buffer);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, 0, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(read_file_into_cbuffer_non_existant_file_returns_null)
        {
            // arrange
            size_t output_size = 100;

            // act
            unsigned char *output_buffer = read_file_into_buffer(TEST_FILE_BAD, &output_size);

            // assert
            ASSERT_IS_NULL(output_buffer);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, 0, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(read_file_into_cbuffer_empty_file_returns_null)
        {
            // arrange
            size_t output_size = 100;

            // act
            unsigned char *output_buffer = read_file_into_buffer(TEST_FILE_EMPTY, &output_size);

            // assert
            ASSERT_IS_NULL(output_buffer);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, 0, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(concat_files_to_cstring_invalid_params)
        {
            // arrange
            char *output_string;
            const char *files[] = {
                TEST_FILE_ALPHA,
                TEST_FILE_NUMERIC
            };

            // act, assert
            output_string = concat_files_to_cstring(NULL, 10);
            ASSERT_IS_NULL(output_string);

            output_string = concat_files_to_cstring(files, 0);
            ASSERT_IS_NULL(output_string);

            // cleanup
        }

        TEST_FUNCTION(concat_files_to_cstring_smoke)
        {
            // arrange
            char *expected_string = "ABCD1234";
            size_t expected_string_size = 9;
            const char *files[] = {
                TEST_FILE_ALPHA,
                TEST_FILE_NUMERIC
            };

            // act
            char *output_string = concat_files_to_cstring(files, sizeof(files)/sizeof(files[0]));
            size_t output_size = strlen(output_string) + 1;

            // assert
            ASSERT_IS_NOT_NULL(output_string);
            int cmp_result = strcmp(expected_string, output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, cmp_result, "Line:" TOSTRING(__LINE__));
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, expected_string_size, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
            free(output_string);
        }

        TEST_FUNCTION(concat_files_to_cstring_with_empty_file_smoke)
        {
            // arrange
            char *expected_string = "ABCD1234";
            size_t expected_string_size = 9;
            const char *files[] = {
                TEST_FILE_ALPHA,
                TEST_FILE_EMPTY,
                TEST_FILE_NUMERIC
            };

            // act
            char *output_string = concat_files_to_cstring(files, sizeof(files)/sizeof(files[0]));
            size_t output_size = strlen(output_string) + 1;

            // assert
            ASSERT_IS_NOT_NULL(output_string);
            int cmp_result = strcmp(expected_string, output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, cmp_result, "Line:" TOSTRING(__LINE__));
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, expected_string_size, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
            free(output_string);
        }

        TEST_FUNCTION(concat_files_to_cstring_with_all_empty_file_smoke)
        {
            // arrange
            char *expected_string = "";
            size_t expected_string_size = 1;
            const char *files[] = {
                TEST_FILE_EMPTY,
                TEST_FILE_EMPTY,
                TEST_FILE_EMPTY,
            };

            // act
            char *output_string = concat_files_to_cstring(files, sizeof(files)/sizeof(files[0]));
            size_t output_size = strlen(output_string) + 1;

            // assert
            ASSERT_IS_NOT_NULL(output_string);
            int cmp_result = strcmp(expected_string, output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, cmp_result, "Line:" TOSTRING(__LINE__));
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, expected_string_size, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
            free(output_string);
        }

        TEST_FUNCTION(concat_files_to_cstring_with_bad_file_returns_null)
        {
            // arrange
            const char *files[] = {
                TEST_FILE_ALPHA,
                TEST_FILE_BAD,
                TEST_FILE_NUMERIC
            };

            // act
            char *output_string = concat_files_to_cstring(files, sizeof(files)/sizeof(files[0]));

            // assert
            ASSERT_IS_NULL(output_string);

            // cleanup
        }

        TEST_FUNCTION(test_is_directory_valid_returns_false_with_bad_dirs)
        {
            // arrange
            bool result;

            // act, assert
            result = is_directory_valid(NULL);
            ASSERT_IS_FALSE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            result = is_directory_valid("");
            ASSERT_IS_FALSE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            result = is_directory_valid("some_bad_dir");
            ASSERT_IS_FALSE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(test_is_directory_valid_returns_true_with_valid_dirs)
        {
            // arrange
            bool result;
            // act, assert
            result = is_directory_valid(".");
            ASSERT_IS_TRUE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            result = is_directory_valid("..");
            ASSERT_IS_TRUE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(test_is_file_valid_returns_false_with_bad_files)
        {
            // arrange
            bool result;

            // act, assert
            result = is_file_valid(NULL);
            ASSERT_IS_FALSE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            result = is_file_valid("");
            ASSERT_IS_FALSE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            result = is_file_valid(TEST_FILE_BAD);
            ASSERT_IS_FALSE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(test_is_file_valid_returns_true_with_valid_files)
        {
            // arrange
            bool result;

            // act, assert
            result = is_file_valid(TEST_FILE_ALPHA);
            ASSERT_IS_TRUE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            result = is_file_valid(TEST_FILE_NUMERIC);
            ASSERT_IS_TRUE_WITH_MSG(result, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(test_write_cstring_to_file_smoke)
        {
            // arrange
            const char *expected_string = "ZZXXYYZZ";
            size_t expected_string_size = 9;
            const char *input_string = "ZZXXYYZZ";
            (void)delete_file(TEST_WRITE_FILE);

            // act
            int output = write_cstring_to_file(TEST_WRITE_FILE, input_string);
            size_t output_size = 0;
            char *output_string = read_file_into_cstring(TEST_WRITE_FILE, &output_size);

            // assert
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));
            ASSERT_IS_NOT_NULL(output_string);
            int cmp_result = strcmp(expected_string, output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, cmp_result, "Line:" TOSTRING(__LINE__));
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, expected_string_size, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
            free(output_string);
        }

        TEST_FUNCTION(test_write_cstring_to_file_invalid_params)
        {
            // arrange
            int output;
            (void)delete_file(TEST_WRITE_FILE);

            // act, assert
            output = write_cstring_to_file(NULL, "abcd");
            ASSERT_ARE_NOT_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));

            output = write_cstring_to_file("", "abcd");
            ASSERT_ARE_NOT_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));

            output = write_cstring_to_file(TEST_WRITE_FILE, NULL);
            ASSERT_ARE_NOT_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(test_write_cstring_to_file_empty_file_returns_null_when_read)
        {
            // arrange
            const char *expected_string = NULL;
            size_t expected_string_size = 0;
            const char *input_string = "";
            (void)delete_file(TEST_WRITE_FILE);

            // act
            int output = write_cstring_to_file(TEST_WRITE_FILE, input_string);
            size_t output_size = 10;
            char *output_string = read_file_into_cstring(TEST_WRITE_FILE, &output_size);

            // assert
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));
            ASSERT_IS_NULL(output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, expected_string_size, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(test_delete_file_smoke)
        {
            // arrange
            size_t expected_string_size = 0;
            const char *input_string = "abcd";

            int status = write_cstring_to_file(TEST_WRITE_FILE_FOR_DELETE, input_string);
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, status, "Line:" TOSTRING(__LINE__));

            // act
            int output = delete_file(TEST_WRITE_FILE_FOR_DELETE);
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));
            size_t output_size = 10;
            char *output_string = read_file_into_cstring(TEST_WRITE_FILE_FOR_DELETE, &output_size);

            // assert
            ASSERT_ARE_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));
            ASSERT_IS_NULL(output_string);
            ASSERT_ARE_EQUAL_WITH_MSG(size_t, expected_string_size, output_size, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

        TEST_FUNCTION(test_delete_file_invalid_params)
        {
            // arrange
            int output;

            // act, assert
            output = delete_file(NULL);
            ASSERT_ARE_NOT_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));

            output = delete_file("");
            ASSERT_ARE_NOT_EQUAL_WITH_MSG(int, 0, output, "Line:" TOSTRING(__LINE__));

            // cleanup
        }

END_TEST_SUITE(edge_hsm_util_int_tests)
