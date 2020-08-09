﻿using Microsoft.AspNetCore.Components;
using ReplyApp.Models;
using System;
using BlazorInputFile;
using System.Linq;
using ReplyApp.Managers;

namespace ReplyApp.Pages.Replys.Components
{
    public partial class EditorForm
    {
        #region Fields
        private string parentId = "";

        protected int[] parentIds = { 1, 2, 3 };

        /// <summary>
        /// 첨부 파일 리스트 보관
        /// </summary>
        private IFileListEntry[] selectedFiles;
        #endregion

        #region Properties
        /// <summary>
        /// 모달 다이얼로그를 표시할건지 여부 
        /// </summary>
        public bool IsShow { get; set; } = false;
        #endregion

        #region Public Methods
        /// <summary>
        /// 폼 보이기 
        /// </summary>
        public void Show()
        {
            IsShow = true; // 현재 인라인 모달 폼 보이기
        }

        /// <summary>
        /// 폼 닫기
        /// </summary>
        public void Hide()
        {
            IsShow = false; // 현재 인라인 모달 폼 숨기기
        }
        #endregion

        #region Parameters
        /// <summary>
        /// 폼의 제목 영역
        /// </summary>
        [Parameter]
        public RenderFragment EditorFormTitle { get; set; } 

        /// <summary>
        /// 넘어온 모델 개체 
        /// </summary>
        [Parameter]
        public Reply Model { get; set; }

        /// <summary>
        /// 부모 컴포넌트에게 생성(Create)이 완료되었다고 보고하는 목적으로 부모 컴포넌트에게 알림
        /// 학습 목적으로 Action 대리자 사용
        /// </summary>
        [Parameter]
        public Action CreateCallback { get; set; }

        /// <summary>
        /// 부모 컴포넌트에게 수정(Edit)이 완료되었다고 보고하는 목적으로 부모 컴포넌트에게 알림
        /// 학습 목적으로 EventCallback 구조체 사용
        /// </summary>
        [Parameter]
        public EventCallback<bool> EditCallback { get; set; }

        [Parameter]
        public string ParentKey { get; set; } = "";
        #endregion

        #region Injectors
        /// <summary>
        /// 리포지토리 클래스에 대한 참조 
        /// </summary>
        [Inject]
        public IReplyRepository RepositoryReference { get; set; }

        [Inject]
        public IFileStorageManager FileStorageManagerReference { get; set; }
        #endregion

        #region Lifecycle Methods
        protected override void OnParametersSet()
        {
            // ParentId가 넘어온 값이 있으면... 즉, 0이 아니면 ParentId 드롭다운 리스트 기본값 선택
            parentId = Model.ParentId.ToString();
            if (parentId == "0")
            {
                parentId = "";
            }
        }  
        #endregion

        #region Event Handlers

        protected async void CreateOrEditClick()
        {
            #region 파일 업로드 관련 추가 코드 영역
            if (selectedFiles != null && selectedFiles.Length > 0)
            {
                // 파일 업로드
                var file = selectedFiles.FirstOrDefault();
                var fileName = "";
                int fileSize = 0;
                if (file != null)
                {
                    //file.Name = $"{DateTime.Now.ToString("yyyyMMddhhmmss")}{file.Name}";
                    fileName = file.Name;
                    fileSize = Convert.ToInt32(file.Size);

                    //[A] byte[] 형태
                    //var ms = new MemoryStream();
                    //await file.Data.CopyToAsync(ms);
                    //await FileStorageManager.ReplyAsync(ms.ToArray(), file.Name, "", true);
                    //[B] Stream 형태
                    //string folderPath = Path.Combine(WebHostEnvironment.WebRootPath, "files");
                    await FileStorageManagerReference.UploadAsync(file.Data, file.Name, "", true);

                    Model.FileName = fileName;
                    Model.FileSize = fileSize;
                }  
            }
            #endregion

            if (!int.TryParse(parentId, out int newParentId))
            {
                newParentId = 0;
            }
            Model.ParentId = newParentId;
            Model.ParentKey = Model.ParentKey; 

            if (Model.Id == 0)
            {
                // Create
                await RepositoryReference.AddAsync(Model);
                CreateCallback?.Invoke(); 
            }
            else
            {
                // Edit
                await RepositoryReference.EditAsync(Model);
                await EditCallback.InvokeAsync(true);
            }
            //IsShow = false; // this.Hide()
        }

        protected void HandleSelection(IFileListEntry[] files)
        {
            this.selectedFiles = files;
        }
        #endregion
    }
}
